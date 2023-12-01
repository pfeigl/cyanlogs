import {Component, OnDestroy, OnInit} from '@angular/core';
import {CommonModule} from '@angular/common';
import {RouterOutlet} from '@angular/router';
import * as signalR from '@microsoft/signalr';
import {FormControl, FormsModule, ReactiveFormsModule} from "@angular/forms";
import {debounceTime} from "rxjs";

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
  imports: [CommonModule, RouterOutlet, FormsModule, ReactiveFormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit, OnDestroy {
  logs: Log[] = [];
  clear = false;

  hubConnection: signalR.HubConnection;
  subscription?: signalR.ISubscription<Log>;

  search$: signalR.Subject<string> = new signalR.Subject<string>();
  searchControl = new FormControl();

  constructor() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl("https://localhost:7255/logs")
      .configureLogging(signalR.LogLevel.Information)
      .build();
  }

  ngOnDestroy(): void {
    this.subscription?.dispose();
  }

  ngOnInit(): void {
    this.hubConnection.start()
      .catch((err) => console.error(err.toString()))
      .then(() => {

        this.subscription = this.hubConnection.stream<Log>("Query", this.search$)
          .subscribe({
            next: (log) => {
              if(this.clear) {
                this.logs = [];
                this.clear = false;
              }
              this.logs.push(log)
            },
            complete: () => {
            },
            error: (err) => {
            }
          });

        this.search$.next("");
      });

    this.searchControl.valueChanges
      .pipe(debounceTime(500))
      .subscribe((v) => {
        this.clear = true;
        this.search$.next(v)
      });
  }
}
