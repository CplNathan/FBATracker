import { ScrapedProduct } from "../models/ProductModels"

export function CalculateCheapest(productListing: ScrapedProduct[]): ScrapedProduct {
    return productListing?.sort((a, b) => a.price - b.price)[0]
}