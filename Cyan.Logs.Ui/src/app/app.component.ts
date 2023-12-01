import {Component, OnInit} from '@angular/core';
import {CommonModule} from '@angular/common';
import {RouterOutlet} from '@angular/router';
import * as signalR from '@microsoft/signalr';

export interface Log {
  "@t": string;
  "@m": string;
  "@l": LogLevel;
  "@x": string;
  [key: string]: string | number | undefined | null;
}

export enum LogLevel {
  Information = "Information",
  Debug = "Debug",
  Error = "Error",
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  title = 'Cyan.Logs.Ui';

  logs: Log[] = [];

  hubConnection: signalR.HubConnection;

  constructor() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl("https://localhost:7255/logs")
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.hubConnection.on('Log', (log: Log) => {
      this.logs.push(log);
    });

  }

  ngOnInit(): void {

    this.hubConnection.start()
      .catch((err) => console.error(err.toString()))
      .then(() => {
        this.hubConnection.invoke('Query', "");
      }
    );

  }


}
