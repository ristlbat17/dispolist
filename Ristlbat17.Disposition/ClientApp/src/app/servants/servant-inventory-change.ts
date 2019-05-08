import { Grade } from "../services/api_client_generated";
export class ServantInventoryChange {
  constructor(public company: string, public location: string, public grade: Grade, public amount: number) { }
}