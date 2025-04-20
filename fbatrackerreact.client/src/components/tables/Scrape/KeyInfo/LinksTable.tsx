import { TableContainer, Paper, Table, TableHead, TableRow, TableCell, TableBody, Button } from "@mui/material";
import { ScrapedProduct, AmazonProduct } from "../../../../models/ProductModels";

const LinksTable = (props: { products: ScrapedProduct[], listing: AmazonProduct }) => {
    return (
        <TableContainer component={Paper}>
            <Table size="small">
                <TableHead>
                    <TableRow>
                        <TableCell align="left">Retailer</TableCell>
                        <TableCell align="left">Price</TableCell>
                        <TableCell align="right"></TableCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    <TableRow>
                        <TableCell align="left">
                            Amazon
                        </TableCell>
                        <TableCell>
                            {props.listing.productPricing.lowestPreferredPrice} GBP
                        </TableCell>
                        <TableCell align="right">
                            <Button size="small" target="_blank" href={props.listing.url}>
                                Visit
                            </Button>
                        </TableCell>
                    </TableRow>
                    {props.products?.map((row) => (
                        <TableRow
                            key={row.source}
                        >
                            <TableCell align="left">
                                {row.source}
                            </TableCell>
                            <TableCell align="left">
                                {row.price} GBP
                            </TableCell>
                            <TableCell align="right">
                                <Button size="small" target="_blank" href={row.url}>
                                    Visit
                                </Button>
                            </TableCell>
                        </TableRow>
                    ))}
                </TableBody>
            </Table>
        </TableContainer>
    )
};

export default LinksTable;