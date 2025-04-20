import { Grid, Paper } from '@mui/material';
import ScrapeStatusTable from '../components/tables/ScrapeStatus/ScrapeStatusTable';
import SystemNotificationList from '../components/SystemNotifcationList';
import { AutoSizer } from 'react-virtualized';

const Settings = () => {
    return (
        <AutoSizer disableWidth>
            {(height) => (
                <Grid container flexDirection={'row'} spacing={2} height={height}>
                    <Grid item xs={12} md={8}>
                        <ScrapeStatusTable />
                    </Grid>
                    <Grid item xs={12} md={4}>
                        <Paper sx={{ overflowY: 'scroll', height: '100%' }}>
                            <SystemNotificationList />
                        </Paper>
                    </Grid>
                </Grid>
            )}
        </AutoSizer>
    )
};

export default Settings;