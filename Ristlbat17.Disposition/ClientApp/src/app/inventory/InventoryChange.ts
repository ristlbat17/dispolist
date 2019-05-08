export class InventoryChange {
  constructor(
    public company: string,
    public location: string,
    public sapNr: string,
    public amount: number
  ) {}
}
