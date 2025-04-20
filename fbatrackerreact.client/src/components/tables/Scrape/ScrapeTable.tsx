import * as React from 'react';
import IconButton from '@mui/material/IconButton';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Paper from '@mui/material/Paper';
import { AmazonProduct, ScrapedProduct, ScrapeModel } from '../../../models/ProductModels';
import FilterListIcon from '@mui/icons-material/FilterList';
import { HiddenSm } from '../../../Shared/StyleConstants';
import FileDownloadIcon from '@mui/icons-material/FileDownload';
import { CalculateCheapest } from '../../../Shared/ProductHelper';
import { ScrapeRow } from './ScrapeRow';
import { AutoSizer } from 'react-virtualized';
import { Badge, Skeleton, TableFooter, TablePagination } from '@mui/material';
import FilterDialog from '../../dialogs/FilterDialog';
import { Filters } from '../../../models/FiltersModel';
import { RequestHelper } from '../../../Shared/RequestHelper';

const ScrapeTable = () => {
    const [dataLoaded, setDataLoaded] = React.useState(false);
    const [filterDialogOpen, setFilterDialogOpen] = React.useState(false);
    const [filters, setFilters] = React.useState<Filters>({ showGated: true, showDangerous: true, showUnknownBSR: false, showUnknownProfit: false, showUnknownSales: false, minSales: 10, maxBSR: 2, minProfit: 0, maxCost: 100, resultListType: 3, showRealtime: false, autoHide: true, showNoBuybox: true } as Filters);
    const [sortedProducts, setSortedProducts] = React.useState<AmazonProduct[]>([]);
    const [asinProducts, setAsinProducts] = React.useState<Map<string, ScrapedProduct[]>>(new Map<string, ScrapedProduct[]>);
    const [page, setPage] = React.useState(0);
    const [rowsPerPage, setRowsPerPage] = React.useState(50);

    const PaginationElement = () => (
        <TablePagination sx={{ width: '100%' }}
            rowsPerPageOptions={[25, 50, 100]}
            count={[...filteredRows?.values() ?? []].length}
            rowsPerPage={rowsPerPage}
            page={page}
            onPageChange={(_event: unknown, newPage: number) => setPage(newPage)}
            onRowsPerPageChange={(event: React.ChangeEvent<HTMLInputElement>) => { setRowsPerPage(parseInt(event.target.value, 10)); setPage(0); }}
        />
    );

    React.useEffect(() => {
        setDataLoaded(false);

        RequestHelper.GetScrape(filters.showRealtime, filters.resultListType).then((json: ScrapeModel) => {
            const amazonMap = new Map<string, AmazonProduct>(Object.entries(json.amazonProducts));
            const productMap = new Map<string, ScrapedProduct[]>(Object.entries(json.scrapedProducts))

            const flattenedProducts = [...productMap.values()].flat();
            const asinProductMapped = [...amazonMap.keys()].flat().map(value => ({ "key": value, "value": flattenedProducts.filter(item => item.amazonStandardIdentificationNumber == value) }));
            const asinProductMap = new Map<string, ScrapedProduct[]>(asinProductMapped.map(value => [value.key, value.value]));

            setAsinProducts(asinProductMap)
            setSortedProducts([...amazonMap.values()].sort((a: AmazonProduct, b: AmazonProduct) => {
                return (CalculateCheapest(asinProductMap.get(b.amazonStandardIdentificationNumber)!)?.profit ?? 0) - (CalculateCheapest(asinProductMap.get(a.amazonStandardIdentificationNumber)!)?.profit ?? 0)
            }));

            setDataLoaded(true);
        })
    }, [filters.showRealtime, filters.resultListType]);

    const filteredRows = React.useMemo(
        () => {
            const filteredRows = sortedProducts
                .filter((value) => value.productEligibility?.isGateLocked == filters.showGated || value.productEligibility?.isGateLocked == false)
                .filter((value) => value.productEligibility?.isRestricted == filters.showDangerous || value.productEligibility?.isRestricted == false)
                .filter((value) => (value.sellerAmpData?.bestSellingRate == undefined && filters.showUnknownBSR) || value.sellerAmpData?.bestSellingRate != undefined)
                .filter((value) => ((value.sellerAmpData?.buyBox == undefined || value.sellerAmpData?.buyBox == false) && filters.showNoBuybox) || value.sellerAmpData?.buyBox == true)
                .filter((value) => ((value.sellerAmpData?.estimatedSales == undefined || value.sellerAmpData?.estimatedSales == "Unknown") && filters.showUnknownSales) || (value.sellerAmpData?.estimatedSales != undefined && value.sellerAmpData?.estimatedSales != "Unknown" && ((Number.parseInt(value.sellerAmpData?.estimatedSales) || filters.minSales) >= filters.minSales)))
                .filter((value) => (CalculateCheapest(asinProducts.get(value.amazonStandardIdentificationNumber)!).profit == undefined && filters.showUnknownProfit) || CalculateCheapest(asinProducts.get(value.amazonStandardIdentificationNumber)!).profit != undefined)
                .filter((value) => {
                    const products = asinProducts.get(value?.amazonStandardIdentificationNumber)!;
                    const profit = CalculateCheapest(products)?.profit ?? -1;

                    return profit >= filters.minProfit
                }).filter((value) => {
                    const products = asinProducts.get(value?.amazonStandardIdentificationNumber)!;
                    const cheapestProduct = CalculateCheapest(products);

                    return cheapestProduct.price <= filters.maxCost;
                });
            return filteredRows;
        },
        [sortedProducts, filters, asinProducts]
    )

    const visibleRows = React.useMemo(
        () => {
            return filteredRows?.slice(
                page * rowsPerPage,
                page * rowsPerPage + rowsPerPage,
            )
        },
        [filteredRows, page, rowsPerPage],
    );

    return (
        <>
            <AutoSizer disableWidth>
                {(height) => (dataLoaded ?
                    <TableContainer component={Paper} sx={{ height: height }}>
                        <Table stickyHeader size="small">
                            <TableHead>
                                <TableRow>
                                    <PaginationElement />
                                </TableRow>
                                <TableRow>
                                    <TableCell align="center">
                                    </TableCell>
                                    <TableCell>Retailer</TableCell>
                                    <TableCell align="left" sx={{ width: "33.33%" }}>Product Name</TableCell>
                                    <TableCell align="center" sx={HiddenSm}>EAN/UPC</TableCell>
                                    <TableCell align="center" sx={HiddenSm}>Retailer Price</TableCell>
                                    <TableCell align="center" sx={HiddenSm}>Profit</TableCell>
                                    <TableCell align="center" sx={HiddenSm}>Sales</TableCell>
                                    <TableCell align="center" sx={HiddenSm}>BSR</TableCell>
                                    <TableCell align="center" sx={HiddenSm}>Warnings</TableCell>
                                    <TableCell align="right">
                                        <IconButton
                                            size="small"
                                            href="/api/scrape/download"
                                            target="_blank"
                                            download
                                        >
                                            <FileDownloadIcon />
                                        </IconButton>
                                        <IconButton
                                            size="small"
                                            onClick={() => setFilterDialogOpen(!filterDialogOpen)}
                                        >
                                            <Badge variant="standard" color="primary" badgeContent={sortedProducts.length - filteredRows.length}>
                                                <FilterListIcon />
                                            </Badge>
                                        </IconButton>
                                    </TableCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                {visibleRows?.map((row) => (
                                    <ScrapeRow key={row.amazonStandardIdentificationNumber} scrapedProducts={asinProducts.get(row.amazonStandardIdentificationNumber)!} amazonProduct={row} />
                                ))}
                            </TableBody>
                            <TableFooter>
                                <TableRow>
                                    <PaginationElement />
                                </TableRow>
                            </TableFooter>
                        </Table>
                    </TableContainer>
                    : <Skeleton variant="rounded" animation="pulse" width={"100%"} sx={{ height: height }} />)}
            </AutoSizer>
            <FilterDialog filterDialogOpen={filterDialogOpen} handleFiltersChanged={setFilters} filters={filters} lowestPrice={CalculateCheapest(asinProducts.get(sortedProducts[sortedProducts.length - 1]?.amazonStandardIdentificationNumber)!)?.profit ?? 0} highestPrice={CalculateCheapest(asinProducts.get(sortedProducts[0]?.amazonStandardIdentificationNumber)!)?.profit ?? 100} />
        </>
    )
};

export default ScrapeTable;