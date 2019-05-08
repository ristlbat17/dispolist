import { Component, OnInit, Input } from "@angular/core";
import { MaterialAllocation } from "../services/api_client_generated";

@Component({
  selector: "material-allocation",
  template: `
    <loading *ngIf="loading"></loading>
    <div *ngIf="allocation">
        <div class="col-sm-2">
         {{ title }}
        </div>
        <div class="col-sm-2">
          Bestand <span class="label label-default">{{allocation.stock}}</span>
        </div>
        <div class="col-sm-2">
          Eingesetzt <span class="label label-default">{{allocation.used}}</span>
        </div>
        <div class="col-sm-2">
          Defekt <span [ngClass]=" allocation.damaged > 0 ? 'label label-danger' : 'label label-default' ">{{allocation.damaged}}</span>
        </div>
        <div class="col-sm-4">
          Verfügbar <span [ngClass]=" allocation.available > 0 ? 'label label-success' : 'label label-warning' ">{{allocation.available}}</span>
        </div>
    </div>
    `
})
export class MaterialAllocationComponent implements OnInit {
  constructor() { }

  @Input()
  loading: boolean;

  @Input()
  allocation: MaterialAllocation;

  @Input()
  title: string;

  ngOnInit() { }
}
