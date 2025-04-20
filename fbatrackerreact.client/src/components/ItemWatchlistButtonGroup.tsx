import React from "react";
import { IconButton } from "@mui/material";
import PlaylistAddIcon from '@mui/icons-material/PlaylistAdd';
import DoneIcon from '@mui/icons-material/Done';
import { AmazonProduct } from "../models/ProductModels";
import { RequestHelper } from "../Shared/RequestHelper";

const ItemWatchlistButtonGroup = (props: { amazonProduct: AmazonProduct }) => {
    const [watched, setWatched] = React.useState<undefined | boolean>(undefined);

    const handleWatchlist = () => {
        RequestHelper.AddWatchlist(props.amazonProduct.amazonStandardIdentificationNumber).then(setWatched);
    }

    return (
        <>
            <IconButton
                size="small"
                onClick={handleWatchlist}
                disabled={watched || props.amazonProduct.productVisibility.isWatchlisted}
            >
                {watched || props.amazonProduct.productVisibility.isWatchlisted ? (<DoneIcon />) : (<PlaylistAddIcon />)}
            </IconButton>
        </>
    )
};

export default ItemWatchlistButtonGroup;