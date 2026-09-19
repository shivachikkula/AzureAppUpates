import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApprovalsService } from '../../core/services/approvals.service';
import { ChangeRequest } from '../../core/models/domain.models';

@Component({
  selector: 'app-approvals',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './approvals.component.html',
  styleUrl: './approvals.component.scss',
})
export class ApprovalsComponent {
  private readonly approvalsService = inject(ApprovalsService);
  private readonly snackBar = inject(MatSnackBar);

  readonly requests = signal<ChangeRequest[]>([]);
  readonly loading = signal(true);
  readonly busyId = signal<string | null>(null);
  readonly notes = new Map<string, string>();

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.approvalsService.getPending().subscribe({
      next: (requests) => {
        this.requests.set(requests);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  approve(request: ChangeRequest): void {
    this.busyId.set(request.id);
    this.approvalsService.approve(request.id, this.notes.get(request.id)).subscribe({
      next: () => {
        this.requests.set(this.requests().filter((r) => r.id !== request.id));
        this.busyId.set(null);
        this.snackBar.open(`Approved and applied change for ${request.applicationName}`, 'Dismiss', {
          duration: 5000,
        });
      },
      error: (err) => {
        this.busyId.set(null);
        this.snackBar.open(err?.error?.message ?? 'Failed to approve the request.', 'Dismiss', { duration: 6000 });
      },
    });
  }

  reject(request: ChangeRequest): void {
    this.busyId.set(request.id);
    this.approvalsService.reject(request.id, this.notes.get(request.id)).subscribe({
      next: () => {
        this.requests.set(this.requests().filter((r) => r.id !== request.id));
        this.busyId.set(null);
        this.snackBar.open(`Rejected change for ${request.applicationName}`, 'Dismiss', { duration: 5000 });
      },
      error: (err) => {
        this.busyId.set(null);
        this.snackBar.open(err?.error?.message ?? 'Failed to reject the request.', 'Dismiss', { duration: 6000 });
      },
    });
  }
}
