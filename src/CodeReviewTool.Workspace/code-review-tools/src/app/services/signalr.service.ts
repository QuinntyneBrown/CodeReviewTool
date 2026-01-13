import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { DiffResult } from '../models/diff-result';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private _hubConnection: signalR.HubConnection | null = null;
  private _diffResultSubject = new BehaviorSubject<DiffResult | null>(null);
  public diffResult$: Observable<DiffResult | null> = this._diffResultSubject.asObservable();

  private _isConnectedSubject = new BehaviorSubject<boolean>(false);
  public isConnected$: Observable<boolean> = this._isConnectedSubject.asObservable();

  constructor() {
    this.initializeConnection();
  }

  private initializeConnection(): void {
    this._hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hub/code-review`)
      .withAutomaticReconnect()
      .build();

    this._hubConnection.on('DiffResultReceived', (result: DiffResult) => {
      this._diffResultSubject.next(result);
    });

    this._hubConnection.onreconnecting(() => {
      this._isConnectedSubject.next(false);
    });

    this._hubConnection.onreconnected(() => {
      this._isConnectedSubject.next(true);
    });

    this._hubConnection.onclose(() => {
      this._isConnectedSubject.next(false);
    });

    this.startConnection();
  }

  private startConnection(): void {
    this._hubConnection?.start()
      .then(() => {
        this._isConnectedSubject.next(true);
      })
      .catch(err => {
        console.error('Error while starting connection: ' + err);
        setTimeout(() => this.startConnection(), 5000);
      });
  }

  disconnect(): void {
    this._hubConnection?.stop();
  }
}
