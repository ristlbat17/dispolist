import { Component, OnInit } from "@angular/core";

@Component({
  template: `
    <ngb-tabset justify="justified">
      <ngb-tab>
        <ng-template ngbTabTitle>
          <span class="fas fa-car-crash fa-2x"></span> Defektmeldung
        </ng-template>
        <ng-template ngbTabContent>
          <inventory-damaged></inventory-damaged>
        </ng-template>
      </ngb-tab>

      <ngb-tab>
        <ng-template ngbTabTitle>
          <span class="fas fa-screwdriver fa-2x"></span> Material repariert
        </ng-template>
        <ng-template ngbTabContent>
          <inventory-repaired></inventory-repaired>
        </ng-template>
      </ngb-tab>

      <ngb-tab>
        <ng-template ngbTabTitle>
          <span class="fa fa-cogs fa-2x"></span> Material eingesetzt
        </ng-template>
        <ng-template ngbTabContent>
          <inventory-used></inventory-used>
        </ng-template>
      </ngb-tab>

      <ngb-tab>
        <ng-template ngbTabTitle>
          <span class="fas fa-box fa-2x"></span> Bestand korrigieren
        </ng-template>
        <ng-template ngbTabContent>
          <inventory-correct-stock></inventory-correct-stock>
        </ng-template>
      </ngb-tab>
    </ngb-tabset>
  `
})
export class ReportMaterialChangesComponent implements OnInit {
  constructor() {}

  ngOnInit() {}

  tab: number = 0;

  setTab(num: number) {
    this.tab = num;
  }

  isSelected(num: number) {
    return this.tab === num;
  }
}
