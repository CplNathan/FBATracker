import { List, ListItem, ListItemText } from "@mui/material";
import { Log } from "../models/LogModel";
import React from "react";
import { RequestHelper } from "../Shared/RequestHelper";
import { AutoSizer } from "react-virtualized";

const SystemNotificationList = () => {
    const [logs, setLogs] = React.useState<Log[]>();

    React.useEffect(() => {
        RequestHelper.GetLogs().then(setLogs);
    }, []);

    return (
        <AutoSizer disableWidth>
            {(height) =>
                <List dense sx={{ paddingY: 0, height: height }}>
                    {logs?.map((row, i) => (
                        <ListItem key={i}>
                            <ListItemText primary="System" secondary={`${row.message} - ${new Date(row.added).toDateString()}`} />
                        </ListItem>
                    ))}

                </List>
            }
        </AutoSizer>
    )
};

export default SystemNotificationList;