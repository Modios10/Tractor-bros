library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity timer_1sec is
    Generic (
        CLK_FREQ : integer := 50_000_000
    );
    Port (
        clk     : in  std_logic;
        pulse   : out std_logic
    );
end timer_1sec;

architecture rtl of timer_1sec is
    constant PERIOD : integer := CLK_FREQ - 1;  -- CLK_FREQ ciclos = 1 segundo
    signal counter : integer range 0 to PERIOD := 0;
begin
    
    process(clk)
    begin
        if rising_edge(clk) then
            if counter < PERIOD then
                counter <= counter + 1;
                pulse <= '0';
            else
                counter <= 0;
                pulse <= '1';  -- Pulse for 1 clock cycle every 1 second
            end if;
        end if;
    end process;

end rtl;
