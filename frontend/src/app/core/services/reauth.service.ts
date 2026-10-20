import { Injectable, signal } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ReauthService {
  readonly isOpen = signal<boolean>(false);
  readonly userEmail = signal<string>('');
  readonly errorMessage = signal<string>('');
  readonly isLoading = signal<boolean>(false);

  private readonly reauthSuccess$ = new Subject<boolean>();

  openModal(email: string): Subject<boolean> {
    this.userEmail.set(email);
    this.errorMessage.set('');
    this.isLoading.set(false);
    this.isOpen.set(true);
    return this.reauthSuccess$;
  }

  closeModal(): void {
    this.isOpen.set(false);
  }

  notifySuccess(): void {
    this.isOpen.set(false);
    this.reauthSuccess$.next(true);
  }

  notifyFailure(message: string): void {
    this.errorMessage.set(message);
    this.isLoading.set(false);
  }
}
