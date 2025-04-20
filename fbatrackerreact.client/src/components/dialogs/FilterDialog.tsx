import React from "react";
import { Checkbox, Dialog, DialogContent, DialogTitle, FormControl, FormControlLabel, FormGroup, Grid, InputLabel, MenuItem, Select, Slider, Switch, TextField, Typography } from "@mui/material";
import { Filters } from "../../models/FiltersModel";
import { RequestHelper } from "../../Shared/RequestHelper";

const FilterDialog = (props: { filterDialogOpen: boolean, filters: Filters, handleFiltersChanged: (filters: Filters) => void, lowestPrice: number, highestPrice: number }) => {
    const [open, setOpen] = React.useState(props.filterDialogOpen);
    const [filters, setFilters] = React.useState<Filters>(props.filters);
    const [lists, setLists] = React.useState<Map<number, string>>(new Map<number, string>);

    React.useEffect(() => {
        props.handleFiltersChanged(filters);
        RequestHelper.GetLists().then(result => setLists(result));
    }, [filters]);

    return (
        <>
            <Dialog
                keepMounted={false}
                open={props.filterDialogOpen !== open}
                fullWidth
                onClose={() => setOpen(!open)}
            >
                <DialogTitle id="alert-dialog-title">
                    Filters
                </DialogTitle>
                <DialogContent>
                    <Grid container paddingTop={2} spacing={2}>
                        <Grid item xs={12}>
                            <FormGroup>
                                <FormControl fullWidth>
                                    <InputLabel>Active List</InputLabel>
                                    <Select label="Active List" defaultValue={3} onChange={(event) => setFilters({ ...filters, resultListType: Number.parseInt(event.target.value as string, 10) })}>
                                        {Object.entries(lists)?.map(item => (
                                            <MenuItem key={item[0]} value={item[0]}>{item[1]}</MenuItem>
                                        ))}
                                    </Select>
                                </FormControl>
                            </FormGroup>
                        </Grid>
                        <Grid item xs={12} md={6}>
                            <Typography variant="subtitle1" component="div">
                                Eligibility
                            </Typography>
                            <FormGroup>
                                <FormControlLabel control={<Checkbox defaultChecked={filters.showGated ? undefined : true} color="warning" onChange={(event) => setFilters({ ...filters, showGated: !event.target.checked })} />} label="Hide Gated" labelPlacement="end"></FormControlLabel>
                                <FormControlLabel control={<Checkbox defaultChecked={filters.showDangerous ? undefined : true} color="error" onChange={(event) => setFilters({ ...filters, showDangerous: !event.target.checked })} />} label="Hide Dangerous" labelPlacement="end"></FormControlLabel>
                            </FormGroup>
                        </Grid>
                        <Grid item xs={12} md={6}>
                            <Typography variant="subtitle1" component="div">
                                Missing Data
                            </Typography>
                            <FormGroup>
                                <FormControlLabel control={<Checkbox defaultChecked={filters.showNoBuybox ? undefined : true} onChange={(event) => setFilters({ ...filters, showNoBuybox: !event.target.checked })} />} label="Hide No BuyBox" labelPlacement="end"></FormControlLabel>
                                <FormControlLabel control={<Checkbox defaultChecked={filters.showUnknownBSR ? undefined : true} onChange={(event) => setFilters({ ...filters, showUnknownBSR: !event.target.checked })} />} label="Hide Unknown BSR" labelPlacement="end"></FormControlLabel>
                                <FormControlLabel control={<Checkbox defaultChecked={filters.showUnknownSales ? undefined : true} onChange={(event) => setFilters({ ...filters, showUnknownSales: !event.target.checked })} />} label="Hide Unknown Sales" labelPlacement="end"></FormControlLabel>
                                <FormControlLabel control={<Checkbox defaultChecked={filters.showUnknownProfit ? undefined : true} onChange={(event) => setFilters({ ...filters, showUnknownProfit: !event.target.checked })} />} label="Hide Unknown Profit" labelPlacement="end"></FormControlLabel>
                            </FormGroup>
                        </Grid>
                        <Grid item xs={12}>
                            <FormGroup>
                                <TextField
                                    label="Min Sales"
                                    type="number"
                                    defaultValue={filters.minSales}
                                    onChange={(event) => { setFilters({ ...filters, minSales: Number.parseInt(event.target.value, 10) }) }}
                                    InputLabelProps={{
                                        shrink: true,
                                    }}
                                />
                            </FormGroup>
                        </Grid>
                        <Grid item xs={12}>
                            <FormGroup>
                                <TextField
                                    label="Max BSR"
                                    type="number"
                                    defaultValue={filters.maxBSR}
                                    onChange={(event) => { setFilters({ ...filters, maxBSR: Number.parseInt(event.target.value, 10) }) }}
                                    InputLabelProps={{
                                        shrink: true,
                                    }}
                                />
                            </FormGroup>
                        </Grid>
                        <Grid item xs={12}>
                            <FormGroup>
                                <FormControlLabel control={
                                    <Slider
                                        defaultValue={filters.minProfit}
                                        min={props.lowestPrice}
                                        max={props.highestPrice}
                                        onChangeCommitted={(_event, value) => { setFilters({ ...filters, minProfit: value as number }) }}
                                        valueLabelDisplay="on" />
                                } labelPlacement="top" label="Min Profit" />
                            </FormGroup>
                        </Grid>
                        <Grid item xs={12}>
                            <FormGroup>
                                <TextField
                                    label="Max Cost"
                                    type="number"
                                    defaultValue={filters.maxCost}
                                    onChange={(event) => { setFilters({ ...filters, maxCost: Number.parseInt(event.target.value, 10) }) }}
                                    InputLabelProps={{
                                        shrink: true,
                                    }}
                                />
                            </FormGroup>
                        </Grid>
                        <Grid item xs={12}>
                            <FormGroup>
                                <FormControlLabel control={<Switch defaultChecked={filters.showRealtime ? true : undefined} onChange={(event) => setFilters({ ...filters, showRealtime: event.target.checked })} inputProps={{ 'aria-label': 'controlled' }} />} label="Realtime Results" labelPlacement="end" />
                            </FormGroup>
                        </Grid>
                    </Grid>
                </DialogContent>
            </Dialog >
        </>
    )
};

export default FilterDialog;