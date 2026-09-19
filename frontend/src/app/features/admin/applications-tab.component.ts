import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { AdminService } from '../../core/services/admin.service';
import { ApplicationAdmin, Team } from '../../core/models/domain.models';

@Component({
  selector: 'app-applications-tab',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatTableModule,
  ],
  templateUrl: './applications-tab.component.html',
  styleUrl: './admin-shared.scss',
})
export class ApplicationsTabComponent {
  private readonly fb = inject(FormBuilder);
  private readonly adminService = inject(AdminService);
  private readonly snackBar = inject(MatSnackBar);

  readonly apps = signal<ApplicationAdmin[]>([]);
  readonly teams = signal<Team[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly columns = ['name', 'environment', 'team', 'resourceGroup', 'actions'];

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(60)]],
    resourceGroup: ['', [Validators.required, Validators.maxLength(90)]],
    subscriptionId: ['', Validators.required],
    environment: ['Development' as const, Validators.required],
    defaultHostName: [''],
    teamId: ['', Validators.required],
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.adminService.getTeams().subscribe({ next: (teams) => this.teams.set(teams) });
    this.adminService.getApplications().subscribe({
      next: (apps) => {
        this.apps.set(apps);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  create(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.adminService.createApplication(this.form.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.form.reset({ environment: 'Development', teamId: '' });
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.snackBar.open(err?.error?.message ?? 'Failed to create application.', 'Dismiss', { duration: 5000 });
      },
    });
  }

  delete(app: ApplicationAdmin): void {
    if (!confirm(`Remove ${app.name} from the tool? This does not delete the Azure resource.`)) {
      return;
    }

    this.adminService.deleteApplication(app.id).subscribe({
      next: () => this.load(),
      error: (err) => this.snackBar.open(err?.error?.message ?? 'Failed to remove application.', 'Dismiss', { duration: 5000 }),
    });
  }
}
