import { Pricing } from "../models/PricingModel";
import { ScrapeItem } from "../models/ScrapeItemModel";
import { EligibilityModel, ScrapeModel } from "../models/ProductModels";
import { Log } from "../models/LogModel";

export abstract class RequestHelper {
    public static async GetScrape(showRealtime: boolean, listType: number): Promise<ScrapeModel> {
        const response = await fetch(`/api/scrape/${(showRealtime) ? "realtime" : "latest"}?listType=${listType}`);
        return response.json() as Promise<ScrapeModel>
    }

    public static async GetScrapeStatus(): Promise<Map<string, ScrapeItem[]>> {
        const response = await fetch('/api/scrape/status');
        return response.json() as Promise<Map<string, ScrapeItem[]>>
    }

    public static async ManualScrape(scrapeSource: string): Promise<boolean> {
        const response = await fetch(`/api/scrape/manual/${scrapeSource}`);
        return response.json() as Promise<boolean>
    }

    public static async GetEligibility(productAsin: string): Promise<EligibilityModel> {
        const response = await fetch(`/api/fba/eligibility?productAsin=${productAsin}`);
        return response.json() as Promise<EligibilityModel>
    }

    public static async GetPricing(productAsin: string) {
        const response = await fetch(`/api/find/pricingfromidentifier?productAsin=${productAsin}`)
        return response.json() as Promise<Pricing>
    }

    public static async AddWatchlist(productAsin: string): Promise<boolean> {
        const response = await fetch(`/api/scrape/watch?productAsin=${productAsin}`, { method: "POST" })
        return response.json() as Promise<boolean>
    }

    public static async MarkViewed(productAsin: string): Promise<boolean> {
        const response = await fetch(`/api/scrape/view?productAsin=${productAsin}`, { method: "POST" });
        return response.json() as Promise<boolean>
    }

    public static async GetLists(): Promise<Map<number, string>> {
        const response = await fetch(`/api/scrape/lists`);
        return response.json() as Promise<Map<number, string>>;
    }

    public static async GetLogs(): Promise<Log[]> {
        const response = await fetch(`/api/logs/recent`);
        return response.json() as Promise<Log[]>;
    }
}