import { Component, OnInit } from "@angular/core";
import { ServantInventoryChange } from "./servant-inventory-change";
import { Ng2IzitoastService } from "ng2-izitoast";
import {
  InventoryClient,
  Grade6,
  PatchAmount,
  SwaggerException
} from "../services/api_client_generated";

@Component({
  selector: "servant-stock-correction",
  template: `
    <servant-change
      [message]="'Bestand korrigieren'"
      [amountMessage]="'Neuer Bestand (absolut)'"
      (save)="correctStock($event)"
    ></servant-change>
  `
})
export class ServantStockCorrectionComponent implements OnInit {
  constructor(
    private inventoryClient: InventoryClient,
    private iziService: Ng2IzitoastService
  ) {}

  ngOnInit() {}

  correctStock($event) {
    let change = $event.change as ServantInventoryChange;
    this.inventoryClient
      .correctServantStock(
        change.company,
        <any>change.grade,
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
