export interface Filters {
    resultListType: number
    
    showGated: boolean
    showDangerous: boolean
    showUnknownBSR: boolean
    showUnknownSales: boolean
    showUnknownProfit: boolean
    showNoBuybox: boolean

    minSales: number
    maxBSR: number
    
    minProfit: number
    maxCost: number

    showRealtime: boolean
}