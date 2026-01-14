import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, Observable, Subject, from, of } from 'rxjs';
import { catchError, filter, tap } from 'rxjs/operators';
import * as signalR from '@microsoft/signalr';
import { environment } from '../environments/environment';
import { NotificationMessage, ComparisonResponse } from '../models/diff-result';

export type ConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

@Injectable({
  providedIn: 'root',
})
export class SignalRService implements OnDestroy {
  private connection: signalR.HubConnection | null = null;

  private readonly connectionStateSubject = new BehaviorSubject<ConnectionState>('disconnected');
  private readonly notificationsSubject = new Subject<NotificationMessage>();
  private readonly comparisonResultsSubject = new Subject<ComparisonResponse>();

  readonly connectionState$ = this.connectionStateSubject.asObservable();
  readonly notifications$ = this.notificationsSubject.asObservable();
  readonly comparisonResults$ = this.comparisonResultsSubject.asObservable();

  connect(): Observable<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return of(undefined);
    }

    this.connectionStateSubject.next('connecting');

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(environment.signalRHubUrl, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          const delay = Math.min(Math.pow(2, retryContext.previousRetryCount) * 1000, 30000);
          return delay;
        },
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupEventHandlers();

    return from(this.connection.start()).pipe(
      tap(() => this.connectionStateSubject.next('connected')),
      catchError((error) => {
        console.error('SignalR connection error:', error);
        this.connectionStateSubject.next('disconnected');
        throw error;
      })
    );
  }

  disconnect(): Observable<void> {
    if (!this.connection) {
      return of(undefined);
    }

    return from(this.connection.stop()).pipe(
      tap(() => {
        this.connectionStateSubject.next('disconnected');
        this.connection = null;
      })
    );
  }

  subscribeToComparison(requestId: string): Observable<void> {
    if (!this.connection) {
      return of(undefined);
    }

    return from(this.connection.invoke('Subscribe', { channels: [`comparison:${requestId}`] }));
  }

  getComparisonResult(requestId: string): Observable<ComparisonResponse> {
    return this.comparisonResults$.pipe(filter((result) => result.requestId === requestId));
  }

  private setupEventHandlers(): void {
    if (!this.connection) return;

    this.connection.on('ReceiveNotification', (notification: NotificationMessage) => {
      this.notificationsSubject.next(notification);

      if (
        notification.type === 'ComparisonCompleted' ||
        notification.type === 'ComparisonResult'
      ) {
        try {
          const result = JSON.parse(notification.payload) as ComparisonResponse;
          this.comparisonResultsSubject.next(result);
        } catch (e) {
          console.error('Failed to parse comparison result:', e);
        }
      }
    });

    this.connection.onreconnecting(() => {
      this.connectionStateSubject.next('reconnecting');
    });

    this.connection.onreconnected(() => {
      this.connectionStateSubject.next('connected');
    });

    this.connection.onclose(() => {
      this.connectionStateSubject.next('disconnected');
    });
  }

  ngOnDestroy(): void {
    this.disconnect().subscribe();
    this.notificationsSubject.complete();
    this.comparisonResultsSubject.complete();
    this.connectionStateSubject.complete();
  }
}
