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
      [message]="'Material wieder repariert'"
      [amountMessage]="'Anzahl repariert'"
      (save)="reportRepaired($event)"
    ></inventory-change>
  `,
  selector: "inventory-repaired"
})
export class RepairedComponent implements OnInit {
  public errorMessage: string;
  constructor(
    private inventoryClient: InventoryClient,
    private iziService: Ng2IzitoastService
  ) {}

  ngOnInit() {}

  reportRepaired($event) {
    let change = $event.change as InventoryChange;
    this.inventoryClient
      .materialRepaired(
        change.company,
        change.location,
        change.sapNr,
        new PatchAmount({ amount: change.amount })
      )
      .subscribe(
        () => {
          this.iziService.success({ message: "Erfolgreich gespeichert" });
        },
        (error: SwaggerException) => {
          this.iziService.error({
            title: "Fehler ist aufgetreten",
            message: error.response
          });
        }
      );
  }
}
