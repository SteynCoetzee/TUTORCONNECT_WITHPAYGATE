import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export interface Toast {
  id: number;
  message: string;
  type: 'error' | 'success' | 'info' | 'warning';
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private _toast$ = new Subject<Toast>();
  readonly toast$ = this._toast$.asObservable();
  private nextId = 1;

  show(message: string, type: Toast['type'] = 'error') {
    this._toast$.next({ id: this.nextId++, message, type });
  }

  error(message: string)   { this.show(message, 'error'); }
  success(message: string) { this.show(message, 'success'); }
  info(message: string)    { this.show(message, 'info'); }
  warning(message: string) { this.show(message, 'warning'); }
}
