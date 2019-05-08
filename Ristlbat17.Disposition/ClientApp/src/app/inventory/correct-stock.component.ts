import { Component, OnInit } from '@angular/core';
import {
  InventoryClient,
  PatchAmount,
  SwaggerException
} from '../services/api_client_generated';
import { InventoryChange } from './InventoryChange';
import { Ng2IzitoastService } from 'ng2-izitoast';

@Component({
  template: `
    <inventory-change
      [message]="'Bestand korrigieren'"
      [amountMessage]="'Neuer Bestand'"
      (save)="correctStock($event)"
    ></inventory-change>
  `,
  selector: 'inventory-correct-stock'
})
export class CorrectStockComponent implements OnInit {
  constructor(
    private inventoryClient: InventoryClient,
    private iziService: Ng2IzitoastService
  ) {}

  ngOnInit() {}

  correctStock($event) {
    const change = $event.change as InventoryChange;
    this.inventoryClient
      .correctMaterialStock(
        change.company,
        change.sapNr,
        change.location,
        new PatchAmount({ amount: change.amount })
      )
      .subscribe(
        () => {
          this.iziService.success({ message: 'Erfolgreich gespeichert' });
        },
        (error: SwaggerException) => {
          this.iziService.error({
            title: 'Fehler ist aufgetreten',
            message: error.response
          });
        }
      );
  }
}
