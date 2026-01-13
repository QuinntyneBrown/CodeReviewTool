import { TestBed } from '@angular/core/testing';
import { SignalRService } from './signalr.service';

// Mock SignalR
const mockHubConnection = {
  on: jest.fn(),
  start: jest.fn().mockResolvedValue(undefined),
  stop: jest.fn().mockResolvedValue(undefined),
  onreconnecting: jest.fn(),
  onreconnected: jest.fn(),
  onclose: jest.fn(),
};

jest.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: jest.fn().mockImplementation(() => ({
    withUrl: jest.fn().mockReturnThis(),
    withAutomaticReconnect: jest.fn().mockReturnThis(),
    build: jest.fn().mockReturnValue(mockHubConnection),
  })),
}));

describe('SignalRService', () => {
  let service: SignalRService;

  beforeEach(() => {
    jest.clearAllMocks();
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

  it('should initialize hub connection on construction', () => {
    expect(mockHubConnection.on).toHaveBeenCalledWith('DiffResultReceived', expect.any(Function));
    expect(mockHubConnection.start).toHaveBeenCalled();
  });

  it('should register reconnecting handler', () => {
    expect(mockHubConnection.onreconnecting).toHaveBeenCalledWith(expect.any(Function));
  });

  it('should register reconnected handler', () => {
    expect(mockHubConnection.onreconnected).toHaveBeenCalledWith(expect.any(Function));
  });

  it('should register close handler', () => {
    expect(mockHubConnection.onclose).toHaveBeenCalledWith(expect.any(Function));
  });

  it('should have disconnect method', () => {
    expect(typeof service.disconnect).toBe('function');
    service.disconnect();
    expect(mockHubConnection.stop).toHaveBeenCalled();
  });
});
