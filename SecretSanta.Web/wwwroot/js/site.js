window.secretSanta = {
  copyText: async text => {
    if (!navigator.clipboard || !window.isSecureContext) return false;
    try { await navigator.clipboard.writeText(text); return true; } catch { return false; }
  }
};
