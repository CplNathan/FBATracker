import AppBar from '@mui/material/AppBar';
import Box from '@mui/material/Box';
import Toolbar from '@mui/material/Toolbar';
import Typography from '@mui/material/Typography';
import IconButton from '@mui/material/IconButton';
import MenuIcon from '@mui/icons-material/Menu';
import QrCodeIcon from '@mui/icons-material/QrCode';
import { Link, Outlet } from 'react-router-dom';
import { Container, Divider, Drawer, List, ListItem, ListItemButton, ListItemIcon, ListItemText } from '@mui/material';
import React from 'react';
import SpaceDashboardIcon from '@mui/icons-material/SpaceDashboard';

const Layout = () => {
    const [open, setOpen] = React.useState(false);

    const toggleDrawer = (newOpen: boolean) => () => {
        setOpen(newOpen);
    };

    return (
        <>
            <Drawer open={open} onClose={toggleDrawer(false)}>
                <Box sx={{ width: 250 }} role="presentation" onClick={toggleDrawer(false)}>
                    <List>
                        <ListItem disablePadding>
                            <ListItemButton LinkComponent={Link} {...{ to: "/" }} >
                                <ListItemIcon>
                                    <SpaceDashboardIcon />
                                </ListItemIcon>
                                <ListItemText primary="Dashboard" />
                            </ListItemButton>
                        </ListItem>
                    </List>
                    <Divider />
                    <List>
                        <ListItem disablePadding>
                            <ListItemButton LinkComponent={Link} {...{ to: "/settings" }}>
                                <ListItemIcon>
                                </ListItemIcon>
                                <ListItemText primary="Settings" />
                            </ListItemButton>
                        </ListItem>
                    </List>
                </Box>
            </Drawer>
            <AppBar position="static">
                <Toolbar>
                    <IconButton
                        size="large"
                        edge="start"
                        sx={{ mr: 2 }}
                        onClick={toggleDrawer(!open)}
                    >
                        <MenuIcon />
                    </IconButton>
                    <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
                        Nathan's FBA Scanner
                    </Typography>
                    <IconButton
                        size="large"
                        edge="start"
                        disabled
                    >
                        <QrCodeIcon />
                    </IconButton>
                </Toolbar>
            </AppBar>
            <Container disableGutters={true} maxWidth={false} sx={{ height: '100vh', padding: '1rem' }}>
                <Outlet />
            </Container>
        </>
    )
};

export default Layout;