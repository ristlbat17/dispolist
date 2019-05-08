import {
  Component,
  OnInit,
  Input,
  Output,
  EventEmitter,
  ChangeDetectionStrategy,
  ChangeDetectorRef
} from "@angular/core";
import {
  Company,
  Material,
  InventoryClient,
  Location,
  MaterialAllocation
} from "../services/api_client_generated";
import { Router } from "@angular/router";
import { InventoryChange } from "./InventoryChange";

@Component({
  selector: "inventory-change",
  templateUrl: "./inventory-change.component.html",
  changeDetection: ChangeDetectionStrategy.Default
})
export class InventoryChangeComponent implements OnInit {
  @Input()
  public message: string;

  @Input()
  public amountMessage: string;

  @Output()
  public save = new EventEmitter();

  companies: Company[];
  selectedCompany: Company;
  selectedLocation: Location;
  materialList: Material[];
  selectedMaterial: Material;
  changeAmount: number;
  allocation: MaterialAllocation;
  loadingAllocation: boolean;

  constructor(
    private inventoryClient: InventoryClient,
    private router: Router,
    private ref: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.inventoryClient
      .getCompanies()
      .subscribe(result => (this.companies = result));

    this.inventoryClient
      .getMaterialList()
      .subscribe(result => (this.materialList = result));
  }
  loadAllocation() {
    this.loadingAllocation = true;
    this.inventoryClient
      .getMaterialInventoryForLocation(
        this.selectedCompany.name,
        this.selectedLocation.name,
        this.selectedMaterial.sapNr
      )
      .subscribe(
        result => {
          this.allocation = result;
          this.loadingAllocation = false;
        },
        error => {
          console.log(error);
          this.allocation = new MaterialAllocation({
            stock: 0,
            used: 0,
            damaged: 0,
            available: 0
          });
          this.loadingAllocation = false;
        }
      );
  }

  public clickSubmit($event) {
    var change = new InventoryChange(
      this.selectedCompany.name,
      this.selectedLocation.name,
      this.selectedMaterial.sapNr,
      this.changeAmount
    );
    this.save.emit({ $event, change });
  }
  cancel() {
    this.selectedCompany = null;
    this.selectedLocation = null;
    this.selectedMaterial = null;
    this.allocation = null;
  }
}
