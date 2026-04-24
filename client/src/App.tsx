import { Navigate, Route, Routes } from "react-router-dom";

import PortfolioLayout from "./components/portfolio-layout";
import PublicOnlyRoute from "./components/public-only-route";
import RequireAuth from "./components/require-auth";
import Assets from "./pages/Assets";
import History from "./pages/History";
import Home from "./pages/Home";
import Login from "./pages/Login";
import Signup from "./pages/Signup";
import Statistics from "./pages/Statistics";
import Wallet from "./pages/Wallet";

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Home />} />
      <Route element={<PublicOnlyRoute />}>
        <Route path="/login" element={<Login />} />
        <Route path="/signup" element={<Signup />} />
      </Route>
      <Route element={<RequireAuth />}>
        <Route element={<PortfolioLayout />}>
          <Route path="/assets" element={<Assets />} />
          <Route path="/history" element={<History />} />
          <Route path="/statistics" element={<Statistics />} />
          <Route path="/wallet" element={<Wallet />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
