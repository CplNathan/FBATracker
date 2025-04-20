import { IconButton, LinearProgress, Skeleton, Table, TableBody, TableCell, TableContainer, TableHead, TableRow } from '@mui/material';
import Paper from '@mui/material/Paper';
import { useState, useEffect } from 'react';
import SyncIcon from '@mui/icons-material/Sync';
import ArrowRightIcon from '@mui/icons-material/ArrowRight';
import { ScrapeItem } from '../../../models/ScrapeItemModel';
import { RequestHelper } from '../../../Shared/RequestHelper';
import { AutoSizer } from 'react-virtualized';

const ScrapeStatusTable = () => {
    const [dataLoaded, setDataLoaded] = useState(false);
    const [requestManual, setRequestManual] = useState<boolean>(false);
    const [scrapeStatus, setScrapeStatus] = useState<Map<string, ScrapeItem[]>>(new Map<string, ScrapeItem[]>);

    const manualScrape = (source: string) => {
        RequestHelper.ManualScrape(source).then(() => {
            setTimeout(() => setRequestManual(!requestManual), 1000);
        });
    };

    useEffect(() => {
        setDataLoaded(false);
        RequestHelper.GetScrapeStatus().then(result => {
            setScrapeStatus(new Map<string, ScrapeItem[]>(Object.entries(result)));
            setDataLoaded(true);
        });

    }, [requestManual]);

    return (
        <AutoSizer disableWidth>
            {(height) => (dataLoaded ?
                <TableContainer component={Paper} sx={{ height: height }} >
                    <Table stickyHeader size="small">
                        <TableHead>
                            <TableRow>
                                <TableCell>Name</TableCell>
                                <TableCell align="center"></TableCell>
                                <TableCell align="center">Elapsed</TableCell>
                                <TableCell align="center">Scraped</TableCell>
                                <TableCell align="center">Amazon</TableCell>
                                <TableCell align="center">Seller Amp</TableCell>
                                <TableCell align="right">
                                    <IconButton
                                        size="small"
                                        disabled
                                    >
                                        <SyncIcon />
                                    </IconButton>
                                </TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {Array.from(scrapeStatus)?.map((row) => (
                                <TableRow
                                    key={row[0]}
                                    sx={{ '&:last-child td, &:last-child th': { border: 0 } }}
                                >
                                    <TableCell component="th" scope="row">{row[0]}</TableCell>
                                    <TableCell align="center" sx={{ width: "100%" }}>
                                        {row[1][0].scrapeEnded ? (
                                            <LinearProgress variant="determinate" value={100} />
                                        ) : (
                                            <LinearProgress />
                                        )}
                                    </TableCell>
                                    <TableCell align="center">{`${Math.round(row[1][0].elapsedSeconds)}`}s</TableCell>
                                    <TableCell align="center">{row[1][0].scrapedProducts}</TableCell>
                                    <TableCell align="center">{row[1][0].amazonScrapedProducts}</TableCell>
                                    <TableCell align="center">{row[1][0].sellerAmpScrapedProducts}</TableCell>
                                    <TableCell align="right">
                                        <IconButton
                                            size="small"
                                            disabled={!row[1][0].scrapeEnded}
                                            onClick={() => manualScrape(row[0])}
                                        >
                                            <ArrowRightIcon />
                                        </IconButton>
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </TableContainer >
                : <Skeleton variant="rounded" animation="pulse" width={"100%"} sx={{ height: height }} />)}
        </AutoSizer>
    )
};

export default ScrapeStatusTable