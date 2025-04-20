export interface ScrapeModel {
    amazonProducts: Map<string, AmazonProduct>
    scrapedProducts: Map<string, ScrapedProduct[]>
}

export interface ScrapedProduct {
    source: string
    name: string
    price: number
    profit: number | undefined
    productCode: string
    amazonStandardIdentificationNumber: string
    url: string
    outOfStock: boolean
}

export interface AmazonProduct {
    name: string
    productPricing: AmazonPricing
    url: string
    amazonStandardIdentificationNumber: string
    productVisibility: ProductVisibility
    productEligibility: EligibilityModel
    sellerAmpData: SellerAmpData
    created: string
}

export interface ProductVisibility {
    productList: number
    isWatchlisted: boolean
}

export interface AmazonPricing {
    lowestPreferredPrice: number
    highestPrice: number
    fees: number
}

export interface EligibilityModel {
    isGateLocked: boolean
    isRestricted: boolean | undefined
    restrictedReason: string[]
    gatedOnwardUrl: string
}

export interface SellerAmpData {
    salesRank: number
    productsInCategory: number
    estimatedSales: string
    privateLabel: number
    privateLabelMessage: string | undefined
    intellectualProperty: number
    intellectualPropertyMessage: string | undefined
    oversize: boolean
    bestSellingRate: number
    buyBox: boolean
}