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
      [message]="'Neue Defektmeldung'"
      [amountMessage]="'Anzahl Defekte'"
      (save)="reportDefect($event)"
    ></inventory-change>
  `,
  selector: "inventory-damaged"
})
export class DamagedComponent implements OnInit {
  constructor(
    private inventoryClient: InventoryClient,
    private iziService: Ng2IzitoastService
  ) {}

  ngOnInit() {}

  reportDefect($event) {
    let change = $event.change as InventoryChange;

    this.inventoryClient
      .materialDefect(
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
