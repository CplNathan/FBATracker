export interface Pricing {
    itemCondition: number
    marketplaceID: string
    asin: string
    sku: unknown
    status: string
    identifier: Identifier
    summary: Summary
    offers: Offer[]
}

export interface Identifier {
    itemCondition: number
    marketplaceId: string
    asin: string
    sellerSKU: unknown
}

export interface Summary {
    totalOfferCount: number
    numberOfOffers: NumberOfOffer[]
    lowestPrices: Price[]
    buyBoxPrices: Price[]
    listPrice: Amount
    competitivePriceThreshold: unknown
    suggestedLowerPricePlusShipping: unknown
    salesRankings: SalesRanking[]
    buyBoxEligibleOffers: BuyBoxEligibleOffer[]
    offersAvailableTime: unknown
}

export interface NumberOfOffer {
    fulfillmentChannel: number
    condition: string
    offerCount: number
}

export interface Price {
    condition: string
    offerType: unknown
    quantityTier: unknown
    quantityDiscountType: number
    landedPrice: Amount
    listingPrice: Amount
    shipping: Amount
    points: unknown
    sellerId: unknown
}

export interface SalesRanking {
    productCategoryId: string
    rank: number
}

export interface BuyBoxEligibleOffer {
    fulfillmentChannel: number
    condition: string
    offerCount: number
}

export interface Offer {
    offerType: unknown
    myOffer: unknown
    subCondition: string
    sellerId: string
    conditionNotes: unknown
    sellerFeedbackRating: SellerFeedbackRating
    shippingTime: ShippingTime
    listingPrice: Amount
    quantityDiscountPrices: unknown
    points: unknown
    shipping: Amount
    shipsFrom?: ShipsFrom
    isFulfilledByAmazon: boolean
    primeInformation?: PrimeInformation
    isBuyBoxWinner: boolean
    isFeaturedMerchant: boolean
}

export interface SellerFeedbackRating {
    sellerPositiveFeedbackRating: number
    feedbackCount: number
}

export interface ShippingTime {
    availabilityType: number
    minimumHours: number
    maximumHours: number
    availableDate: unknown
}

export interface Amount {
    currencyCode: string
    amount: number
}

export interface ShipsFrom {
    state?: string
    country: string
}

export interface PrimeInformation {
    isPrime: boolean
    isNationalPrime: boolean
}
