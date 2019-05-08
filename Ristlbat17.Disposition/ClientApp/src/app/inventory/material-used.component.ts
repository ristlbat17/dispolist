import { Component, OnInit } from "@angular/core";
import {
  InventoryClient,
  PatchAmount,
  SwaggerException
} from "../services/api_client_generated";
import { InventoryChange } from "./InventoryChange";
import { Ng2IzitoastService } from "ng2-izitoast";

@Component({
  template: `
    <inventory-change
      [message]="'Material eingesetzt'"
      [amountMessage]="'Anzahl neu eingesetzt (+/-)'"
      (save)="reportUsage($event)"
    ></inventory-change>
  `,
  selector: "inventory-used"
})
export class MaterialUsedComponent implements OnInit {
  constructor(
    private inventoryClient: InventoryClient,
    private iziService: Ng2IzitoastService
  ) {}

  ngOnInit() {}

  reportUsage($event) {
    let change = $event.change as InventoryChange;
    this.inventoryClient
      .materialUsed(
        change.company,
        change.sapNr,
        change.location,
        new PatchAmount({ amount: change.amount })
      )
      .subscribe(
        () => {
          this.iziService.success({ message: "Erfolgreich gespeichert" });
        },
        (error: SwaggerException) => {
          this.iziService.error({ message: error.response });
        }
      );
  }
}
