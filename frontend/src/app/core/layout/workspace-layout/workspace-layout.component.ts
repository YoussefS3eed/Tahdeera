import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterOutlet } from '@angular/router';
import { HeaderComponent } from '../../../shared/components/header/header.component';
import { SidebarComponent } from '../../../shared/components/sidebar/sidebar.component';
import { MobileNavComponent } from '../../../shared/components/mobile-nav/mobile-nav.component';

@Component({
  selector: 'app-workspace-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    RouterOutlet,
    HeaderComponent,
    SidebarComponent,
    MobileNavComponent
  ],
  template: `
    <div class="workspace-shell">
      <!-- Desktop Sidebar (Hidden on Mobile) -->
      <div class="desktop-sidebar-container">
        <app-sidebar></app-sidebar>
      </div>

      <!-- Main Column -->
      <div class="main-column">
        <!-- Universal Header -->
        <app-header (toggleSidebar)="toggleMobileDrawer()"></app-header>

        <!-- Main Content Viewport -->
        <main class="content-viewport">
          <router-outlet></router-outlet>
        </main>

        <!-- Mobile Bottom Nav (Visible on Mobile) -->
        <div class="mobile-nav-container">
          <app-mobile-nav></app-mobile-nav>
        </div>
      </div>

      <!-- Mobile Full Drawer Overlay -->
      @if (isMobileDrawerOpen) {
        <div class="mobile-drawer-backdrop" (click)="closeMobileDrawer()">
          <div class="mobile-drawer-content" (click)="$event.stopPropagation()">
            <div class="drawer-header-bar">
              <span class="drawer-title">القائمة الجانبية</span>
              <button
                type="button"
                class="btn-close-mobile-drawer"
                (click)="closeMobileDrawer()"
                aria-label="إغلاق القائمة"
              >
                <span class="material-symbols-outlined">close</span>
              </button>
            </div>
            <div class="drawer-sidebar-body">
              <app-sidebar (tabSelected)="closeMobileDrawer()"></app-sidebar>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
      height: 100vh;
      overflow: hidden;
      background-color: var(--bg-app, #f8fafc);
      color: var(--text-primary, #0f172a);
    }

    .workspace-shell {
      display: flex;
      height: 100vh;
      width: 100%;
      overflow: hidden;
      position: relative;
    }

    .desktop-sidebar-container {
      display: none;
      flex-shrink: 0;
      height: 100%;
      z-index: 20;
    }

    @media (min-width: 769px) {
      .desktop-sidebar-container {
        display: flex;
      }
    }

    .main-column {
      flex: 1;
      display: flex;
      flex-direction: column;
      min-width: 0;
      height: 100%;
      overflow: hidden;
      position: relative;
    }

    .content-viewport {
      flex: 1;
      overflow-y: auto;
      padding-block: 1.5rem calc(5rem + env(safe-area-inset-bottom, 0px));
      padding-inline: 1rem;
    }

    @media (min-width: 769px) {
      .content-viewport {
        padding-block: 2rem;
        padding-inline: 2rem;
      }
    }

    .mobile-nav-container {
      display: block;
      flex-shrink: 0;
    }

    @media (min-width: 769px) {
      .mobile-nav-container {
        display: none;
      }
    }

    /* Mobile Full Drawer Overlay */
    .mobile-drawer-backdrop {
      position: fixed;
      inset: 0;
      z-index: 55;
      background-color: rgba(15, 23, 42, 0.65);
      backdrop-filter: blur(4px);
      display: flex;
    }

    .mobile-drawer-content {
      width: 280px;
      height: 100%;
      background-color: var(--bg-sidebar, #081e3f);
      display: flex;
      flex-direction: column;
      animation: slide-in 0.2s ease-out;
    }

    .drawer-header-bar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding-block: 1rem;
      padding-inline: 1.25rem;
      border-block-end: 1px solid rgba(255, 255, 255, 0.08);
      color: #ffffff;
    }

    .drawer-title {
      font-size: 0.9375rem;
      font-weight: 700;
      color: #ffffff;
    }

    .btn-close-mobile-drawer {
      background: transparent;
      border: none;
      color: #cbd5e1;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 4px;
      border-radius: 6px;
    }

    .drawer-sidebar-body {
      flex: 1;
      overflow-y: auto;
    }

    @keyframes slide-in {
      from { transform: translateX(100%); }
      to { transform: translateX(0); }
    }
  `]
})
export class WorkspaceLayoutComponent {
  isMobileDrawerOpen: boolean = false;

  toggleMobileDrawer(): void {
    this.isMobileDrawerOpen = !this.isMobileDrawerOpen;
  }

  closeMobileDrawer(): void {
    this.isMobileDrawerOpen = false;
  }
}
