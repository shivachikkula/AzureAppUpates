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
import { AppAssignmentAdmin, ApplicationAdmin } from '../../core/models/domain.models';

@Component({
  selector: 'app-developer-assignments-tab',
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
  templateUrl: './developer-assignments-tab.component.html',
  styleUrl: './admin-shared.scss',
})
export class DeveloperAssignmentsTabComponent {
  private readonly fb = inject(FormBuilder);
  private readonly adminService = inject(AdminService);
  private readonly snackBar = inject(MatSnackBar);

  readonly apps = signal<ApplicationAdmin[]>([]);
  readonly assignments = signal<AppAssignmentAdmin[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly columns = ['applicationName', 'userDisplayName', 'userEmail', 'actions'];

  readonly form = this.fb.nonNullable.group({
    applicationId: ['', Validators.required],
    userObjectId: ['', Validators.required],
    userEmail: ['', [Validators.required, Validators.email]],
    userDisplayName: ['', Validators.required],
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.adminService.getApplications().subscribe({ next: (apps) => this.apps.set(apps) });
    this.adminService.getAppAssignments().subscribe({
      next: (assignments) => {
        this.assignments.set(assignments);
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
    this.adminService.createAppAssignment(this.form.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.form.reset({ applicationId: '' });
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.snackBar.open(err?.error?.message ?? 'Failed to grant access.', 'Dismiss', { duration: 5000 });
      },
    });
  }

  delete(assignment: AppAssignmentAdmin): void {
    this.adminService.deleteAppAssignment(assignment.id).subscribe({
      next: () => this.load(),
      error: (err) => this.snackBar.open(err?.error?.message ?? 'Failed to revoke access.', 'Dismiss', { duration: 5000 }),
    });
  }
}
