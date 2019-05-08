import { Component, OnInit, Input } from "@angular/core";
import { ServantAllocation } from "../services/api_client_generated";

@Component({
  selector: "servant-allocation",
  template: `
    <loading *ngIf="loading"></loading>
    <div *ngIf="allocation">
        <div class="col-sm-3">
          Bestand <span class="label label-default">{{allocation.stock}}</span>
        </div>
        <div class="col-sm-3">
          Eingesetzt <span class="label label-default">{{allocation.used}}</span>
        </div>
        <div class="col-sm-3">
          Detachiert <span [ngClass]=" allocation.detached > 0 ? 'label label-warning' : 'label label-default' ">{{allocation.detached}}</span>
        </div>
        <div class="col-sm-3">
          Verfügbar <span [ngClass]=" allocation.available > 0 ? 'label label-success' : 'label label-danger' ">{{allocation.available}}</span>
        </div>
    </div>
    `
})
export class ServantAllocationComponent implements OnInit {
  constructor() {}

 @Input()
 loading: boolean;

  @Input()
  allocation: ServantAllocation;

  ngOnInit() {}
}
