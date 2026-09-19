import { Injectable, NgZone, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LogEntry } from '../models/domain.models';

@Injectable({ providedIn: 'root' })
export class LogsService {
  private readonly zone = inject(NgZone);
  private connection: signalR.HubConnection | null = null;
  private currentApplicationId: string | null = null;

  /** Opens (or reuses) the SignalR connection and subscribes to live log lines for an app. */
  streamLogs(applicationId: string, accessTokenFactory: () => Promise<string>): Observable<LogEntry> {
    return new Observable<LogEntry>((subscriber) => {
      let disposed = false;

      const start = async () => {
        try {
          await this.ensureConnected(accessTokenFactory);
          if (disposed || !this.connection) {
            return;
          }
          this.connection.on('LogReceived', (entry: LogEntry) => {
            this.zone.run(() => subscriber.next(entry));
          });
          if (this.currentApplicationId && this.currentApplicationId !== applicationId) {
            await this.connection.invoke('Unsubscribe', this.currentApplicationId);
          }
          this.currentApplicationId = applicationId;
          await this.connection.invoke('Subscribe', applicationId);
        } catch (err) {
          this.zone.run(() => subscriber.error(err));
        }
      };

      start();

      return () => {
        disposed = true;
        this.connection?.off('LogReceived');
        if (this.currentApplicationId) {
          this.connection?.invoke('Unsubscribe', this.currentApplicationId).catch(() => undefined);
          this.currentApplicationId = null;
        }
      };
    });
  }

  async dispose(): Promise<void> {
    await this.connection?.stop();
    this.connection = null;
    this.currentApplicationId = null;
  }

  private async ensureConnected(accessTokenFactory: () => Promise<string>): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return;
    }
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiBaseUrl.replace(/\/api$/, '')}/hubs/logs`, {
        accessTokenFactory,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    await this.connection.start();
  }
}
