import Chip from '@mui/material/Chip';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Paper from '@mui/material/Paper';
import Badge from '@mui/material/Badge';
import { Skeleton } from '@mui/material';
import React from 'react';
import { Pricing } from '../../../../models/PricingModel';
import { RequestHelper } from '../../../../Shared/RequestHelper';

const SellersTable = (props: { productAsin: string }) => {
    const [pricing, setPricing] = React.useState<Pricing>();

    React.useEffect(() => {
        RequestHelper.GetPricing(props.productAsin).then(setPricing);
    }, [props.productAsin]);

    return (
        pricing ? (
            <TableContainer component={Paper}>
                <Table size="small">
                    <TableHead>
                        <TableRow>
                            <TableCell></TableCell>
                            <TableCell align="left">Price</TableCell>
                            <TableCell align="right">Quantity</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {pricing?.offers?.sort(y => y.isFulfilledByAmazon ? -1 : 1 || y.listingPrice)?.map((row) => (
                            <TableRow
                                key={row.sellerId}
                                sx={{ '&:last-child td, &:last-child th': { border: 0 } }}
                            >
                                <TableCell component="th" scope="row">
                                    <Badge badgeContent={row.isBuyBoxWinner ? "BB" : null} color="success">
                                        <Chip variant={row.isFulfilledByAmazon ? "filled" : "outlined"} label={row.isFulfilledByAmazon ? "FBA" : "FBM"} />
                                    </Badge>
                                </TableCell>
                                <TableCell align="left">{`${row.listingPrice.amount} ${row.listingPrice.currencyCode}`}</TableCell>
                                <TableCell align="right">-</TableCell>
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </TableContainer>
        ) : <Skeleton variant="rounded" sx={{ minHeight: "200px", height: "100%", width: "100%" }} animation="wave" />
    )
};

export default SellersTable;