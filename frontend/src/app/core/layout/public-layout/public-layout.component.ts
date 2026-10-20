import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-public-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet],
  template: `
    <div class="public-layout-shell">
      <main class="public-viewport">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      width: 100%;
      min-height: 100vh;
    }

    .public-layout-shell {
      display: flex;
      flex-direction: column;
      min-height: 100vh;
      background-color: var(--bg-page, #f8fafc);
      color: var(--text-primary, #0f172a);
    }

    .public-viewport {
      flex: 1;
      display: flex;
      flex-direction: column;
      width: 100%;
    }
  `]
})
export class PublicLayoutComponent {}
