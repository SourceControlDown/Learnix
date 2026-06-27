module.exports = {
  // Prettier for frontend files
  "learnix-client/src/**/*.{ts,tsx,js,jsx,css,scss,md}": (filenames) => {
    const files = filenames.join(' ');
    return [
      `prettier --write --ignore-unknown ${files}`,
      "npm run type-check --prefix learnix-client"
    ];
  },
  
  // Dotnet Format for backend C# files
  "Learnix.Backend/**/*.cs": (filenames) => {
    // dotnet format requires the --include flag followed by a space-separated list of files
    // If filenames is too long (Windows max command line length limit is 8191 chars), this might fail,
    // but for typical commits it's perfectly fine.
    const files = filenames.join(' ');
    return [
      `dotnet format Learnix.Backend/Learnix.Backend.slnx --no-restore --include ${files}`,
      "dotnet build Learnix.Backend/Learnix.Backend.slnx"
    ];
  }
};
