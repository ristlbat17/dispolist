import { Component, OnInit } from '@angular/core';

@Component({
  template: `
    <ngb-tabset justify="justified">
      <ngb-tab>
        <ng-template ngbTabTitle>
          <span class="fa fa-user-injured fa-2x"></span> Detachiert
        </ng-template>
        <ng-template ngbTabContent>
          <servant-detached></servant-detached>
        </ng-template>
      </ngb-tab>

      <ngb-tab>
        <ng-template ngbTabTitle>
          <span class="fa fa-user-check fa-2x"></span> Zurück/Verfügbar
        </ng-template>
        <ng-template ngbTabContent>
          <servant-available></servant-available>
        </ng-template>
      </ngb-tab>

      <ngb-tab>
        <ng-template ngbTabTitle>
          <span class="fa fa-user-clock fa-2x"></span> Ada eingesetzt
        </ng-template>
        <ng-template ngbTabContent> <servant-used></servant-used> </ng-template>
      </ngb-tab>

      <ngb-tab>
        <ng-template ngbTabTitle>
          <span class="fa fa-user-edit fa-2x"></span> Bestand korrigieren
        </ng-template>
        <ng-template ngbTabContent>
          <servant-stock-correction></servant-stock-correction>
        </ng-template>
      </ngb-tab>

      <ngb-tab>
        <ng-template ngbTabTitle>
          <span class="fa fa-user-astronaut fa-2x"></span> Soll(OTF)
        </ng-template>
        <ng-template ngbTabContent>
          <servant-ideal-correction></servant-ideal-correction>
        </ng-template>
      </ngb-tab>
    </ngb-tabset>
  `
})
export class ReportServantsChangesComponent implements OnInit {
  constructor() {}

  ngOnInit() {}

}
