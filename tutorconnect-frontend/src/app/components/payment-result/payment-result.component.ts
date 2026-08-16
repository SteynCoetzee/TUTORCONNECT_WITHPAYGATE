import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-payment-result',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './payment-result.component.html',
  styleUrl: './payment-result.component.css'
})
export class PaymentResultComponent implements OnInit {
  status: 'success' | 'cancel' | 'failed' | 'timeout' | 'unknown' = 'unknown';

  constructor(private route: ActivatedRoute, private router: Router) {}

  ngOnInit() {
    const s = this.route.snapshot.queryParamMap.get('status');
    if      (s === 'success') this.status = 'success';
    else if (s === 'cancel')  this.status = 'cancel';
    else if (s === 'failed')  this.status = 'failed';
    else if (s === 'timeout') this.status = 'timeout';
    else                      this.status = 'unknown';
  }

  goToModules() {
    this.router.navigate(['/dashboard/courses']);
  }

  tryAgain() {
    this.router.navigate(['/dashboard/courses']);
  }
}
