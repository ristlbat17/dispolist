import { Component, OnInit, Input } from "@angular/core";
import { ServantAllocation } from "../services/api_client_generated";

@Component({
  selector: 'servant-allocation',
  template: `
  <loading *ngIf="loading"></loading>
  <div *ngIf="allocation">
      <div class="col-sm-2">
        Bestand <span class="label label-default">{{allocation.stock}}</span>
      </div>
      <div class="col-sm-2">
        Eingesetzt <span class="label label-default">{{allocation.used}}</span>
      </div>
      <div class="col-sm-2">
        Detachiert <span [ngClass]=" allocation.detached > 0 ? 'label label-danger' : 'label label-default' ">{{allocation.detached}}</span>
      </div>
      <div class="col-sm-4">
        Verfügbar <span [ngClass]=" allocation.available > 0 ? 'label label-success' : 'label label-warning' ">{{allocation.available}}</span>
      </div>
      <!--<div class="col-sm-2">
       {{title}}
      </div>-->
  </div>
  `
})
export class ServantAllocationComponent implements OnInit {
  constructor() { }

  @Input()
  loading: boolean;

  @Input()
  allocation: ServantAllocation;

  ngOnInit() { }
}
