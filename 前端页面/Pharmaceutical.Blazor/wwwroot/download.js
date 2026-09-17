window.downloadFileFromBytes = (fileName, base64) => {
    const link = document.createElement('a');
    link.href = 'data:text/csv;charset=utf-8;base64,' + base64;
    link.download = fileName;
    link.click();
};
