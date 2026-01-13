import { TestBed } from '@angular/core/testing';
import { SignalRService } from './signalr.service';

// Mock SignalR
jest.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: jest.fn().mockImplementation(() => ({
    withUrl: jest.fn().mockReturnThis(),
    withAutomaticReconnect: jest.fn().mockReturnThis(),
    build: jest.fn().mockReturnValue({
      on: jest.fn(),
      start: jest.fn().mockResolvedValue(undefined),
      stop: jest.fn().mockResolvedValue(undefined),
      onreconnecting: jest.fn(),
      onreconnected: jest.fn(),
      onclose: jest.fn(),
    }),
  })),
}));

describe('SignalRService', () => {
  let service: SignalRService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SignalRService],
    });

    service = TestBed.inject(SignalRService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should expose diffResult$ observable', (done) => {
    service.diffResult$.subscribe(result => {
      expect(result).toBeNull();
      done();
    });
  });

  it('should expose isConnected$ observable', (done) => {
    service.isConnected$.subscribe(isConnected => {
      expect(typeof isConnected).toBe('boolean');
      done();
    });
  });

  it('should have disconnect method', () => {
    expect(typeof service.disconnect).toBe('function');
    expect(() => service.disconnect()).not.toThrow();
  });
});
