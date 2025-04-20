import { TableContainer, Paper, Table, TableHead, TableRow, TableCell, TableBody, Chip } from "@mui/material";
import { EligibilityRow } from "./EligibilityRow";
import { AmazonProduct } from "../../../../models/ProductModels";

const KeyInfoTable = (props: { listing: AmazonProduct }) => {
    return (
        <TableContainer component={Paper}>
            <Table size="small">
                <TableHead>
                    <TableRow>
                        <TableCell align="left">Info</TableCell>
                        <TableCell align="right"></TableCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    <EligibilityRow Asin={props.listing.amazonStandardIdentificationNumber} />
                    <TableRow>
                        <TableCell align="left">
                            BSR
                        </TableCell>
                        <TableCell align="right">
                            <Chip color="primary" label={props.listing.sellerAmpData?.bestSellingRate ? `${props.listing.sellerAmpData.bestSellingRate.toFixed(2)}%` : "Unknown"} />
                        </TableCell>
                    </TableRow>
                    <TableRow>
                        <TableCell align="left">
                            Intellectual Property
                        </TableCell>
                        <TableCell align="right">
                            <Chip color={props.listing.sellerAmpData?.intellectualProperty == 1 ? "warning" : props.listing.sellerAmpData?.intellectualProperty > 1 ? "error" : "primary"} label={props.listing.sellerAmpData?.intellectualProperty != undefined ? `${props.listing.sellerAmpData.intellectualPropertyMessage}` : "Unknown"} />
                        </TableCell>
                    </TableRow>
                    <TableRow>
                        <TableCell align="left">
                            Private Label
                        </TableCell>
                        <TableCell align="right">
                            <Chip color={props.listing.sellerAmpData?.intellectualProperty == 1 ? "warning" : props.listing.sellerAmpData?.intellectualProperty > 1 ? "error" : "primary"} label={props.listing.sellerAmpData?.privateLabel != undefined ? `${props.listing.sellerAmpData.privateLabelMessage}` : "Unknown"} />
                        </TableCell>
                    </TableRow>
                </TableBody>
            </Table>
        </TableContainer>
    )
};

export default KeyInfoTable;