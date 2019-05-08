import {
  Component,
  OnInit,
  Input,
  Output,
  EventEmitter
} from '@angular/core';
import {
  Company,
  InventoryClient,
  Location,
  Grade,
  ServantAllocation
} from '../services/api_client_generated';
import { ServantInventoryChange } from './servant-inventory-change';

@Component({
  selector: 'servant-change',
  templateUrl: './servant-change.component.html'
})
export class ServantChangeComponent implements OnInit {
  @Input()
  public message: string;

  @Input()
  public amountMessage: string;

  @Output()
  public save = new EventEmitter();

  companies: Company[];
  selectedCompany: Company;
  selectedLocation: Location;
  gradeList: Grade[];
  selectedGrade: Grade;
  changeAmount: number;
  allocation: ServantAllocation;
  loadingAllocation: boolean;

  constructor(
    private inventoryClient: InventoryClient) { }

  async ngOnInit() {
    this.companies = await this.inventoryClient.getCompanies().toPromise();
    this.gradeList = [Grade.Offizere, Grade.HoehereUnteroffiziere, Grade.Unteroffiziere, Grade.Mannschaft];
  }

  loadAllocation() {
    this.loadingAllocation = true;
    this.inventoryClient
      .getServantInventoryForLocation(
        this.selectedCompany.name,
        this.selectedLocation.name,
        this.selectedGrade
      )
      .subscribe(
        result => {
          this.allocation = result;
          this.loadingAllocation = false;
        },
        error => {
          console.log(error);
          this.allocation = new ServantAllocation({
            stock: 0,
            used: 0,
            detached: 0,
            available: 0

          });
          this.loadingAllocation = false;
        }
      );
  }

  public clickSubmit($event) {
    const change = new ServantInventoryChange(
      this.selectedCompany.name,
      this.selectedLocation.name,
      this.selectedGrade,
      this.changeAmount
    );
    this.save.emit({ $event, change });
    this.loadAllocation();
  }

  cancel() {
    this.selectedCompany = null;
    this.selectedLocation = null;
    this.selectedGrade = null;
    this.allocation = null;
  }
}


