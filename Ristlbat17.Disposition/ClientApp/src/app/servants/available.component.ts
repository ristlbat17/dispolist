import { Component, OnInit } from "@angular/core";
import {
  InventoryClient,
  PatchAmount,
  SwaggerException,
  Grade4
} from "../services/api_client_generated";
import { Ng2IzitoastService } from "ng2-izitoast";
import { ServantInventoryChange } from "./servant-inventory-change";

@Component({
  selector: "servant-available",
  template: `
    <servant-change
      [message]="'Wieder Verfügbar'"
      [amountMessage]="'Anzahl wieder zur Verfügung stehnder Ada'"
      (save)="detachedReported($event)"
    ></servant-change>
  `
})
export class ServantAvailableComponent implements OnInit {
  constructor(
    private inventoryClient: InventoryClient,
    private iziService: Ng2IzitoastService
  ) {}

  ngOnInit() {}

  detachedReported($event) {
    let change = $event.change as ServantInventoryChange;
    this.inventoryClient
      .servantReturned(
        change.company,
        change.location,
        <any>change.grade,
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
