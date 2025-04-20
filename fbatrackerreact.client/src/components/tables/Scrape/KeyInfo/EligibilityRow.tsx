import { TableRow, TableCell, Chip, Tooltip, IconButton } from "@mui/material";
import React from "react";
import { EligibilityModel } from "../../../../models/ProductModels";
import { RequestHelper } from "../../../../Shared/RequestHelper";

export const EligibilityRow = (props: { Asin: string }) => {
    const [eligibility, setEligibility] = React.useState<EligibilityModel>();

    React.useEffect(() => {
        RequestHelper.GetEligibility(props.Asin).then(setEligibility)
    }, [props.Asin]);

    return (
        <>
            <TableRow>
                <TableCell align="left">
                    Gate Locked
                </TableCell>
                <TableCell align="right">
                    {eligibility?.isGateLocked ? (
                        <IconButton sx={{ padding: 0 }} target="_blank" href={eligibility.gatedOnwardUrl}>
                            <Chip color={"warning"} label={"Yes"} />
                        </IconButton>
                    ) : (
                        <Chip color={eligibility?.isGateLocked == undefined ? "primary" : "success"} label={eligibility?.isGateLocked == undefined ? "Unknown" : "No"} />
                    )}
                </TableCell>
            </TableRow>
            <TableRow>
                <TableCell align="left">
                    Restricted
                </TableCell>
                <TableCell align="right">
                    <Tooltip title={eligibility?.restrictedReason}>
                        <Chip color={eligibility?.isRestricted ? "error" : eligibility?.isRestricted == undefined ? "primary" : "success"} label={eligibility?.isRestricted ? "Yes" : eligibility?.isRestricted == undefined ? "Unknown" : "No"} />
                    </Tooltip>
                </TableCell>
            </TableRow>
        </>
    )
};