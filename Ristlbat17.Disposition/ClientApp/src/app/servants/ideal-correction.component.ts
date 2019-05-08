import { Component, OnInit } from "@angular/core";
import { ServantInventoryChange } from "./servant-inventory-change";
import { Ng2IzitoastService } from "ng2-izitoast";
import {
  InventoryClient,
  Grade6,
  PatchAmount,
  SwaggerException,
  Company,
  Grade
} from "../services/api_client_generated";

@Component({
  selector: "servant-ideal-correction",
  templateUrl: "ideal-correction.component.html"
})
export class ServantIdealCorrectionComponent implements OnInit {
  companies: Company[];
  gradeList: Grade[];
  selectedCompany: Company;
  selectedGrade: Grade;
  changeAmount: number;

  constructor(
    private inventoryClient: InventoryClient,
    private iziService: Ng2IzitoastService
  ) {}

  ngOnInit() {
    this.inventoryClient
      .getCompanies()
      .subscribe(result => (this.companies = result));

    this.gradeList = [
      Grade.Offizere,
      Grade.HoehereUnteroffiziere,
      Grade.Unteroffiziere,
      Grade.Mannschaft
    ];
  }

  cancel() {
    this.selectedCompany = null;
    this.selectedGrade = null;
  }

  correctIdeal() {

    this.inventoryClient
      .correctIdealForCompany(
        this.selectedCompany.name,
        <any>this.selectedGrade,
        new PatchAmount({ amount: this.changeAmount })
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
