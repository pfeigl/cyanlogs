import {Component, OnInit} from '@angular/core';
import {CommonModule} from '@angular/common';
import {RouterOutlet} from '@angular/router';
import * as signalR from '@microsoft/signalr';
import {FormsModule} from "@angular/forms";

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
  imports: [CommonModule, RouterOutlet, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  search = "";

  logs: Log[] = [];

  hubConnection: signalR.HubConnection;
  subscription?: signalR.ISubscription<Log>;

  constructor() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl("https://localhost:7255/logs")
      .configureLogging(signalR.LogLevel.Information)
      .build();

  }

  ngOnInit(): void {
    this.hubConnection.start()
      .catch((err) => console.error(err.toString()))
      .then(() => {
        this.doSearch();
      });

  }

  doSearch() {
    if(this.subscription) {
      this.subscription.dispose();
    }
    this.logs = [];

    this.subscription = this.hubConnection.stream<Log>("Query", this.search)
      .subscribe({
        next: (log) => this.logs.push(log),
        complete: () => { },
        error: (err) => { }
      });
  }
}
