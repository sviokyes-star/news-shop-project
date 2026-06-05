
import { Toaster } from "@/components/ui/toaster";
import { Toaster as Sonner } from "@/components/ui/sonner";
import { TooltipProvider } from "@/components/ui/tooltip";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import MainLayout from "./layouts/MainLayout";
import Index from "./pages/Index";
import News from "./pages/News";
import Shop from "./pages/Shop";
import Servers from "./pages/Servers";
import Tournaments from "./pages/Tournaments";
import Partners from "./pages/Partners";
import TournamentDetail from "./pages/TournamentDetail";
import Profile from "./pages/Profile";
import UserProfile from "./pages/UserProfile";
import NewsDetail from "./pages/NewsDetail";
import Admin from "./pages/Admin";
import NotFound from "./pages/NotFound";

const queryClient = new QueryClient();

const App = () => (
  <QueryClientProvider client={queryClient}>
    <TooltipProvider>
      <Toaster />
      <Sonner />
      <BrowserRouter>
        <Routes>
          <Route element={<MainLayout />}>
            <Route path="/" element={<Index />} />
            <Route path="/news" element={<News />} />
            <Route path="/shop" element={<Shop />} />
            <Route path="/servers" element={<Servers />} />
            <Route path="/tournaments" element={<Tournaments />} />
            <Route path="/partners" element={<Partners />} />
            <Route path="/tournament/:id" element={<TournamentDetail />} />
            <Route path="/news/:id" element={<NewsDetail />} />
            <Route path="/profile" element={<Profile />} />
            <Route path="/profile/:steamId" element={<UserProfile />} />
          </Route>
          <Route path="/admin" element={<Admin />} />
          <Route path="*" element={<NotFound />} />
        </Routes>
      </BrowserRouter>
    </TooltipProvider>
  </QueryClientProvider>
);

export default App;