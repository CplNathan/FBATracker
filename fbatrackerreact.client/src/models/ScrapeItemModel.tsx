export interface ScrapeItem {
    source: string
    scrapedProducts: number
    amazonScrapedProducts: number
    sellerAmpScrapedProducts: number
    scrapeStarted: string
    scrapeEnded: string | undefined
    elapsedSeconds: number
}