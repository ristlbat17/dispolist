import { Component, OnInit } from "@angular/core";
import { ServantInventoryChange } from "./servant-inventory-change";
import { Ng2IzitoastService } from "ng2-izitoast";
import {
  InventoryClient,
  Grade5,
  PatchAmount,
  SwaggerException
} from "../services/api_client_generated";

@Component({
  selector: "servant-used",
  template: `
    <servant-change
      [message]="'Ada eingesetzt'"
      [amountMessage]="'Anzahl neu eingesetzt (+/-)'"
      (save)="reportUsage($event)"
    ></servant-change>
  `
})
export class ServantUsedComponent implements OnInit {
  constructor(
    private inventoryClient: InventoryClient,
    private iziService: Ng2IzitoastService
  ) {}

  ngOnInit() {}

  reportUsage($event) {
    let change = $event.change as ServantInventoryChange;
    this.inventoryClient
      .servantUsed(
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
