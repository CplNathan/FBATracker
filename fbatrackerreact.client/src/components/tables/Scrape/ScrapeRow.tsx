import React from "react";
import { TableRow, TableCell, IconButton, Badge, Collapse, Box, Grid, useMediaQuery, useTheme, Dialog, DialogActions, DialogContent, DialogTitle, Paper } from "@mui/material";
import ItemWatchlistButtonGroup from "../../ItemWatchlistButtonGroup";
import KeyboardArrowDownIcon from '@mui/icons-material/KeyboardArrowDown';
import KeyboardArrowUpIcon from '@mui/icons-material/KeyboardArrowUp';
import { HiddenSm } from "../../../Shared/StyleConstants";
import { AmazonProduct, ScrapedProduct } from "../../../models/ProductModels";
import { CalculateCheapest } from "../../../Shared/ProductHelper";
import TravelExploreIcon from '@mui/icons-material/TravelExplore';
import OpenInFullIcon from '@mui/icons-material/OpenInFull';
import { RequestHelper } from "../../../Shared/RequestHelper";
import CopyrightIcon from '@mui/icons-material/Copyright';
import LabelImportantIcon from '@mui/icons-material/LabelImportant';
import FenceIcon from '@mui/icons-material/Fence';
import BatteryAlertIcon from '@mui/icons-material/BatteryAlert';
import KeyInfoTable from "./KeyInfo/KeyInfoTable";
import LinksTable from "./KeyInfo/LinksTable";
import PricingTable from "./KeyInfo/PricingTable";

const ScrapeDetails = (props: { scrapedProducts: ScrapedProduct[], amazonProduct: AmazonProduct }) => {
    return (
        <Grid container spacing={2}>
            <Grid item xs={12} md={3}>
                <PricingTable productAsin={props.amazonProduct.amazonStandardIdentificationNumber} />
            </Grid>
            <Grid item xs={12} md={6}>
                <KeyInfoTable listing={props.amazonProduct} />
            </Grid>
            <Grid item xs={12} md={3}>
                <LinksTable products={props.scrapedProducts} listing={props.amazonProduct} />
            </Grid>
        </Grid>
    )
};

export const ScrapeRow = (props: { scrapedProducts: ScrapedProduct[], amazonProduct: AmazonProduct }) => {
    const { scrapedProducts, amazonProduct } = props;
    const [open, setOpen] = React.useState(false);
    const [useDialog, setUseDialog] = React.useState(false);
    const [cheapestRow, setCheapestRow] = React.useState<ScrapedProduct | undefined>();
    const [isFocused, setIsFocused] = React.useState<boolean>(false);

    const theme = useTheme();
    const matches = useMediaQuery(theme.breakpoints.down('md'));

    React.useEffect(() => {
        setUseDialog(matches);
        setCheapestRow(CalculateCheapest(scrapedProducts));
    }, [scrapedProducts, matches]);

    React.useMemo(() => {
        if (open && !isFocused) {
            RequestHelper.MarkViewed(amazonProduct.amazonStandardIdentificationNumber).then(setIsFocused);
        }
    }, [open])

    return (
        <>
            <TableRow sx={{ '&:last-child td, &:last-child th': { border: 0 } }} component={Paper} elevation={isFocused || open ? 5 : 1}>
                <TableCell align="center">
                    <IconButton
                        size="small"
                        onClick={() => setOpen(!open)}
                    >
                        {useDialog ? <OpenInFullIcon /> : open ? <KeyboardArrowUpIcon /> : <KeyboardArrowDownIcon />}
                    </IconButton>
                </TableCell>
                <TableCell component="th" scope="row">
                    <Badge variant="standard" badgeContent={scrapedProducts.length > 1 ? scrapedProducts.length : null} color="warning">
                        {scrapedProducts[0].source}
                    </Badge>
                </TableCell>
                <TableCell align="left">
                    {amazonProduct.name}
                </TableCell>
                <TableCell align="center" sx={HiddenSm}>{cheapestRow?.productCode}</TableCell>
                <TableCell align="center" sx={HiddenSm}>{`${cheapestRow?.price?.toFixed(2)} GBP`}</TableCell>
                <TableCell align="center" sx={HiddenSm}>{cheapestRow?.profit ? `${cheapestRow?.profit?.toFixed(2)} GBP` : "Unknown"}</TableCell>
                <TableCell align="center" sx={HiddenSm}>{amazonProduct.sellerAmpData?.estimatedSales ? amazonProduct.sellerAmpData?.estimatedSales : "Unknown"}</TableCell>
                <TableCell align="center" sx={HiddenSm}>{amazonProduct.sellerAmpData?.bestSellingRate ? `${amazonProduct.sellerAmpData.bestSellingRate?.toFixed(2)}%` : "Unknown"}</TableCell>
                <TableCell align="center" sx={HiddenSm}>
                    <IconButton size="small" disabled><CopyrightIcon color={amazonProduct.sellerAmpData?.intellectualPropertyMessage ? (amazonProduct.sellerAmpData.intellectualProperty == 1 ? "warning" : amazonProduct.sellerAmpData.intellectualProperty > 1 ? "error" : "success") : undefined} /></IconButton>
                    <IconButton size="small" disabled><LabelImportantIcon color={amazonProduct.sellerAmpData?.privateLabelMessage ? (amazonProduct.sellerAmpData.privateLabel == 1 ? "warning" : amazonProduct.sellerAmpData.privateLabel > 1 ? "error" : "success") : undefined} /></IconButton>
                    <IconButton size="small" disabled><FenceIcon color={amazonProduct.productEligibility?.isGateLocked != undefined ? (amazonProduct.productEligibility.isGateLocked ? "warning" : "success") : undefined} /></IconButton>
                    <IconButton size="small" disabled><BatteryAlertIcon color={amazonProduct.productEligibility?.isRestricted != undefined ? (amazonProduct.productEligibility.isRestricted ? "error" : "success") : undefined} /></IconButton>
                </TableCell>
                <TableCell align="right">
                    <ItemWatchlistButtonGroup amazonProduct={amazonProduct} />
                    <IconButton
                        className="SaSExTaP5Dc32"
                        data-asin={amazonProduct.amazonStandardIdentificationNumber}
                        data-sas_cost_price={cheapestRow?.price}
                        data-sas_sale_price={amazonProduct.productPricing.lowestPreferredPrice}
                        href="https://selleramp.com"
                        data-source_url="https://source.com"
                        size="small"
                        onClick={() => setIsFocused(true)}
                    >
                        <TravelExploreIcon />
                    </IconButton>
                </TableCell>
            </TableRow>
            {useDialog ? (
                <Dialog
                    keepMounted={false}
                    open={open}
                    fullWidth
                    onClose={() => setOpen(false)}
                >
                    <DialogTitle id="alert-dialog-title">
                        {amazonProduct.name}
                    </DialogTitle>
                    <DialogContent>
                        <ScrapeDetails scrapedProducts={scrapedProducts} amazonProduct={amazonProduct} />
                    </DialogContent>
                    <DialogActions>
                        <ItemWatchlistButtonGroup amazonProduct={amazonProduct} />
                    </DialogActions>
                </Dialog>
            ) : (
                <TableRow>
                    <TableCell style={{ padding: 0 }} colSpan={12}>
                        <Collapse in={open} mountOnEnter unmountOnExit timeout="auto">
                            <Box sx={{ margin: 2 }}>
                                <ScrapeDetails scrapedProducts={scrapedProducts} amazonProduct={amazonProduct} />
                            </Box>
                        </Collapse>
                    </TableCell>
                </TableRow >
            )}
        </>
    );
}