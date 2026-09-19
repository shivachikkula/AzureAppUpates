import { Component, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { ApplicationsService } from '../../core/services/applications.service';
import { AzureApplication } from '../../core/models/domain.models';

@Component({
  selector: 'app-my-apps',
  standalone: true,
  imports: [MatCardModule, MatChipsModule, MatIconModule, MatProgressSpinnerModule, RouterLink],
  templateUrl: './my-apps.component.html',
  styleUrl: './my-apps.component.scss',
})
export class MyAppsComponent {
  private readonly applicationsService = inject(ApplicationsService);

  readonly apps = signal<AzureApplication[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    this.applicationsService.getMyApplications().subscribe({
      next: (apps) => {
        this.apps.set(apps);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load your assigned applications. Please try again later.');
        this.loading.set(false);
      },
    });
  }

  environmentClass(env: string): string {
    return `env-chip env-${env.toLowerCase()}`;
  }
}
