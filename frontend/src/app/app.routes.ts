import { Routes } from '@angular/router';
import { MsalGuard } from '@azure/msal-angular';
import { ShellComponent } from './core/layout/shell.component';
import { adminGuard } from './core/guards/admin.guard';
import { managerGuard } from './core/guards/manager.guard';

export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
    canActivate: [MsalGuard],
    canActivateChild: [MsalGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'my-apps' },
      {
        path: 'my-apps',
        loadComponent: () => import('./features/my-apps/my-apps.component').then((m) => m.MyAppsComponent),
        title: 'My Apps',
      },
      {
        path: 'connection-strings',
        loadComponent: () =>
          import('./features/connection-strings/connection-strings.component').then(
            (m) => m.ConnectionStringsComponent,
          ),
        title: 'Connection Strings',
      },
      {
        path: 'approvals',
        canActivate: [managerGuard],
        loadComponent: () => import('./features/approvals/approvals.component').then((m) => m.ApprovalsComponent),
        title: 'Approvals',
      },
      {
        path: 'logs',
        loadComponent: () => import('./features/logs/logs.component').then((m) => m.LogsComponent),
        title: 'Application Logs',
      },
      {
        path: 'admin',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/admin/admin.component').then((m) => m.AdminComponent),
        title: 'Admin',
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
