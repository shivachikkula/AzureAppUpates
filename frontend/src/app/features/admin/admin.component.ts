import { Component } from '@angular/core';
import { MatTabsModule } from '@angular/material/tabs';
import { ApplicationsTabComponent } from './applications-tab.component';
import { DeveloperAssignmentsTabComponent } from './developer-assignments-tab.component';
import { ManagerAssignmentsTabComponent } from './manager-assignments-tab.component';
import { TeamsTabComponent } from './teams-tab.component';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [
    MatTabsModule,
    TeamsTabComponent,
    ApplicationsTabComponent,
    DeveloperAssignmentsTabComponent,
    ManagerAssignmentsTabComponent,
  ],
  templateUrl: './admin.component.html',
  styleUrl: './admin-shared.scss',
})
export class AdminComponent {}
