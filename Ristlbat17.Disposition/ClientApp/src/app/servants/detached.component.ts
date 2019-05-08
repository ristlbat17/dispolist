import { Component, OnInit } from '@angular/core';
import {
  InventoryClient,
  Grade6,
  PatchAmount,
  SwaggerException,
  Grade3
} from '../services/api_client_generated';
import { Ng2IzitoastService } from 'ng2-izitoast';
import { ServantInventoryChange } from './servant-inventory-change';

@Component({
  selector: 'servant-detached',
  template: `
    <servant-change
      [message]="'Detachiert Meldung'"
      [amountMessage]="'Anzahl detachierte Ada'"
      (save)="detachedReported($event)"
    ></servant-change>
  `
})
export class ServantDetachedComponent implements OnInit {
  constructor(
    private inventoryClient: InventoryClient,
    private iziService: Ng2IzitoastService
  ) {}

  ngOnInit() {}

  detachedReported($event) {
    const change = $event.change as ServantInventoryChange;
    this.inventoryClient
      .servantDetached(
        change.company,
        change.location,
        <any>change.grade,
        new PatchAmount({ amount: change.amount })
      )
      .subscribe(
        () => {
          this.iziService.success({ message: 'Erfolgreich gespeichert' });
        },
        (error: SwaggerException) => {
          this.iziService.error({ message: error.response });
        }
      );
  }
}
